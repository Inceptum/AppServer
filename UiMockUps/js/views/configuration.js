define([
    'jquery',
    'backbone',
    'underscore',
    'views/confirm',
    'views/alerts',
    'text!templates/configuration.html','fileupload'],
    function($, Backbone, _, confirmView,alerts, template){
        var TreeView = Backbone.View.extend({
            tagName: "li",
            folderTemplate:'<span><a href="#" data-toggle="collapse" data-target="#<%= model.uiid%>"><i class="<%=model.imgClass%>"></i></a><a href="#/configurations/<%=model.configuration%>/<%=model.id%>"><%= model.name%></a></span>',
            leafTemplate:'<span><i class="icon-file"></i><a href="#/configurations/<%=model.configuration%>/<%=model.id%>"><%= model.name%></a></span>',
            initialize: function(){
                this.collection = this.model.bundles;
            },
            render:function(){
                var escapedId=this.model.id.split(".").join("\\.");

                if(this.model.bundles.length>0){
                    if(this.model.expanded=== undefined || !this.model.expanded)
                        this.model.expanded=this.model.bundles.length<2;//expand bundles with single child
                    var imgClass=this.model.expanded?"icon-chevron-down":"icon-chevron-right";

                    this.template = _.template( this.folderTemplate, { model:_.extend(this.model.toJSON(),{uiid:escapedId,imgClass:imgClass}) } );
                    $(this.el).html(this.template);

                    var ul = new TreeRoot({model:this.model,visible:this.model.expanded}).render().el;
                    $(this.el).append(ul);
                    var model=this.model;
                    $(this.el).find("a").first().click(function(e){
                        $(e.target).toggleClass("icon-chevron-right").toggleClass("icon-chevron-down");
                        model.expanded=!model.expanded;
                        e.preventDefault();
                    });
                }
                else{
                    this.template = _.template( this.leafTemplate, { model: this.model.toJSON() } );
                    $(this.el).html(this.template);
                }
                return this;
            }
        });

        var TreeRoot = Backbone.View.extend({
            tagName: "ul",
            className:"collapse nav nav-list",
            render:function(){
                var el=$(this.el);
                el.attr("id",this.model.id);
                this.model.bundles.each(function(node){
                    el.append(new TreeView({model:node}).render().el);
                });
                if(this.options.visible)
                    $(el).addClass("in");
                return this;
            }
        });

        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                _.bindAll(this,"upload");
            },events:{
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
            destroy:function(){
                var self=this;
                confirmView.open({title:"Delete",body:"You are about to delete '"+this.model.id+"' configuration. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        self.model.destroy({
                            wait: true,
                            error:function(model,response){
                                alerts.show({
                                    type:"error",
                                    text:"Failed to delete instance '"+self.model.id+"'. "+JSON.parse(response.responseText).Error});
                            }
                        });
                    });
            },
            render: function(){
                this.template = _.template( template, {model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find("#confTree").append(new TreeRoot({model:this.model,visible:true}).render().el);
                this.importDialog=$("#importDialog");
                $('#fakeInputFile').val("").next().click(function(){$('#inputFile').click();});
                return this;
            },
            'dispose':function(){

            }
        });

        return View;
    });
