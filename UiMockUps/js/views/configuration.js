define([
    'jquery',
    'backbone',
    'underscore',
    'views/confirm',
    'views/alerts',
    'text!templates/configuration/configuration.html',
    'text!templates/configuration/treeNode.html',
    'bootbox',
    'fileupload'],
    function($, Backbone, _, confirmView,alerts, template,treeNodeTemplate){
        var TreeNodeView = Backbone.View.extend({
            tagName: "li",
            initialize: function(){
                _(this).bindAll("deleteBundle","dispose");
                this.collection = this.model.bundles;
            },events:{
                "click a.delete:first":"deleteBundle"
            },
            deleteBundle:function(model){
                this.trigger('delete',(arguments[0] instanceof jQuery.Event)?this.model:model,this);
            },
            render:function(){
                var escapedId=this.model.id.split(".").join("\\.");
                var isFolder=this.model.bundles.length>0;
                var imgClass;
                var extraProperties={};

                if(isFolder){
                    if(this.model.expanded=== undefined || !this.model.expanded)
                        this.model.expanded=this.model.bundles.length<2;//expand bundles with single child
                    imgClass=this.model.expanded?"icon-chevron-down":"icon-chevron-right";
                    extraProperties = {uiid:escapedId,imgClass:imgClass,isFolder:isFolder};
                }

                this.template = _.template( treeNodeTemplate, { model:_.extend(this.model.toJSON(),extraProperties) } );
                $(this.el).html(this.template).find("div").first().hover(function(){$(this).find(".btn-group").toggleClass("hide")});

                if(isFolder){
                    this.subview = new TreeView({model:this.model,visible:this.model.expanded}).render();
                    this.subview.bind("delete", this.deleteBundle);
                    $(this.el).append(this.subview.el);
                    var model=this.model;
                    $(this.el).find("a").first().click(function(e){
                        $(e.target).toggleClass("icon-chevron-right").toggleClass("icon-chevron-down");
                        model.expanded=!model.expanded;
                        e.preventDefault();
                    });
                }
                return this;
            },
            dispose:function(){
                if(this.subview){
                    this.subview.unbind("delete", this.deleteBundle)
                    this.subview.close();
                }
            }
        });

        var TreeView = Backbone.View.extend({
            tagName: "ul",
            className:"collapse nav nav-list",
            initialize: function(){
                _(this).bindAll("deleteBundle","dispose");
                this.subviews=[];
            },
            render:function(){
                var el=$(this.el);
                el.attr("id",this.model.id);
                var self = this;
                this.model.bundles.each(function(node){
                    var treeView = new TreeNodeView({model:node});
                    el.append(treeView.render().el);
                    treeView.bind("delete", self.deleteBundle);
                    self.subviews.push(treeView);
                });
                if(this.options.visible)
                    $(el).addClass("in");
                return this;
            },
            deleteBundle:function(model){
                this.trigger('delete',model,this);
            },
            dispose:function(){
                var self = this;
                _.each(this.subviews,function(subview){
                    subview.unbind("delete", self.deleteBundle)
                    subview.close();
                });
            }
        });

        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                _.bindAll(this,"upload","dispose","deleteBundle");
            },
            events:{
                "click #delete":"destroy",
                "click #import":"upload",
                "click #importSubmit":"doImport"
            },
            upload:function(){

                var input = $('#inputFile').unbind();
                input.after(input.clone(true).change(function() {
                    $('#fakeInputFile').val($(this).val());
                })).remove();
                $('#fakeInputFile').val("");
                $('#progress .bar').css('width','0%');
                var self=this;
                this.uploader=this.importDialog.find("#inputFile").fileupload({
                    dataType: 'json',
                    multipart:false,
                    autoUpload:false,
                    fileInput:null,
                    done: function (e, data) {
                        $('#progressAlert').addClass("hide");
                        self.model.fetch({async:false});
                        self.render();
                    },
                    fail: function (e, data) {
                        $('#progressAlert').addClass("hide");
                        self.model.fetch({async:false});
                        self.render();
                    },
                    progressall: function (e, data) {
                        var progress = parseInt(data.loaded / data.total * 100, 10);
                        $('#progress .bar').css(
                            'width',
                            progress + '%'
                        );
                    }
                });
                this.importDialog.modal();
            },
            doImport:function(){
                $('#progressAlert').removeClass("hide");
                this.importDialog.modal('hide');
                this.uploader.fileupload('send',{fileInput: this.uploader});
            },
            deleteBundle:function(model){
                var self=this;
                confirmView.open({title:"Delete",body:"You are about to delete '"+model.id+"' bundle. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        model.destroy({
                            wait: true,
                            success:function(){
                              self.render();
                            },
                            error:function(model,response){
                                var error;
                                try{
                                    error=JSON.parse(response.responseText).Error;
                                }catch(e){
                                    error=response.responseText;
                                    if(!error)
                                        error="Response status code:"+response.status;
                                }
                                alerts.show({
                                    type:"error",
                                    text:"Failed to delete bundle '"+model.id+"'. "+error});
                            }
                        });
                    });
            },
            destroy:function(){
                var self=this;
                confirmView.open({title:"Delete",body:"You are about to delete '"+this.model.id+"' configuration. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        self.model.destroy({
                            wait: true,
                            success:function(){
                                alerts.show({
                                    type:"info",
                                    text:"Configuration '"+self.model.id+"' deleted."});
                                self.navigate('#/configurations', true);
                            },
                            error:function(model,response){
                                alerts.show({
                                    type:"error",
                                    text:"Failed to delete configuration '"+self.model.id+"'. "+JSON.parse(response.responseText).Error});
                            }
                        });
                    });
            },
            render: function(){
                if(this.treeView){
                    this.treeView.close();
                    this.treeView=null;
                    $(this.el).html('');
                }
                this.template = _.template( template, {model: this.model.toJSON() } );
                $(this.el).html(this.template);
                this.treeView = new TreeView({model:this.model,visible:true});
                $(this.el).find("#confTree").append(this.treeView.render().el);
                this.importDialog=$("#importDialog");
                $('#fakeInputFile').val("").next().click(function(){$('#inputFile').click();});
                this.treeView.bind("delete", this.deleteBundle);
                return this;
            },
            'dispose':function(){
                this.treeView.unbind("delete", this.deleteBundle);
                this.treeView.close();
            }
        });

        return View;
    });
