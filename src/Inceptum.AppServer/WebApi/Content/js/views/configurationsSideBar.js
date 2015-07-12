define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/configuration/configurationsSideBar.html',
    'collections/configurations',
    'views/confirm',
    'views/alerts',
    'text!templates/configuration/treeNode.html'],
    function($, Backbone, _, template,Configurations,confirmView,alerts,treeNodeTemplate){
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
                    this.subview = new TreeView({model:this.model,visible:this.model.expanded,isLeaf:true}).render();
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
                if(this.options.isLeaf)
                    el.attr("id",this.model.id);
                var self = this;
                if (this.model) {
                    this.model.bundles.each(function(node) {
                        var treeView = new TreeNodeView({ model: node });
                        el.append(treeView.render().el);
                        treeView.bind("delete", self.deleteBundle);
                        self.subviews.push(treeView);
                    });
                }
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
            el:'#sidebar',
            initialize: function(options){
                this.activeItem=options.active;
                this.activeBundle=options.activeBundle;

                this.template = _.template( template, { model: Configurations.toJSON() } );
                _.bindAll(this,"dispose","deleteBundle");

                if(!(typeof this.activeBundle === 'undefined')){
                    var bundleToExpand="";
                    var config=Configurations.get(this.activeItem);
                    _.each(this.activeBundle.split('.'), function(current){
                        bundleToExpand=(bundleToExpand==""?"":bundleToExpand+".")+current;
                        config.getBundle(bundleToExpand).expanded=true;
                    });
                }

            },
            events:{
                "click .create":"createConfig"
            },
            createConfig:function(e){
                e.preventDefault();
                var self = this;
                bootbox.prompt("Configuration Name", function(result) {
                    if(result){
                        Configurations.create({name: result},{
                            wait: true,
                            success:function(){
                                alerts.show({
                                    type:"info",
                                    text:"Configuration '"+result+"' created."});
                                self.navigate("#configurations/"+result);
                            },
                            error:function(model,response){
                                alerts.show({
                                    type:"error",
                                    text:"Failed to create configuration '"+result+"'. "+JSON.parse(response.responseText).Error});
                            }

                        });
                    }
                });
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
            render: function(){
                if(this.treeView){
                    this.treeView.close();
                    this.treeView=null;
                    $(this.el).html('');
                }
                $(this.el).html(this.template);

                var menuItem= $(this.el).find('*[data-id="'+this.activeItem+'"]').addClass("active");
                this.treeView = new TreeView({model:Configurations.get(this.activeItem),visible:true});
                menuItem.append(this.treeView.render().el);

                var menuItem= $(this.el).find('a.bundleName[data-id="'+this.activeBundle+'"]').addClass("active");
                this.importDialog=$("#importDialog");
                $('#fakeInputFile').val("").next().click(function(){$('#inputFile').click();});
                this.treeView.bind("delete", this.deleteBundle);
            },
            'dispose':function(){
                this.treeView.unbind("delete", this.deleteBundle);
                this.treeView.close();
            }
        });

        return View;
    });
/**
 * Created with JetBrains WebStorm.
 * User: knst
 * Date: 07.10.12
 * Time: 16:08
 * To change this template use File | Settings | File Templates.
 */
