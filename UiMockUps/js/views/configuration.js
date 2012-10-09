define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/configuration.html'],
    function($, Backbone, _, template){
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
            },
            render: function(){
                this.template = _.template( template, {model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find("#confTree").append(new TreeRoot({model:this.model,visible:true}).render().el);

                //  $(this.el).find("#confTree li").collapse();
                return this;
            },
            'dispose':function(){

            }
        });

        return View;
    });
