define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/configuration.html'],
    function($, Backbone, _, template){
        var TreeView = Backbone.View.extend({
            tagName: "li",
            initialize: function(){
                this.collection = this.model.bundles;
            },
            render:function(){
                var escapedId=this.model.id.replace(".","\\.");

                if(this.model.bundles.length>0){
                    this.template = _.template( '<span><a href="#" data-toggle="collapse" data-target="#<%= model.id%>"><i class="icon-chevron-down "></i></a><a href="#"><%= model.name%></a></span>', { model:_.extend(this.model.toJSON(),{id:escapedId}) } );
                    $(this.el).html(this.template);
                    $(this.el).append(new TreeRoot({model:this.model}).render().el);
                    $(this.el).find("a:first").click(function(e){
                        $(e.target).toggleClass("icon-chevron-right").toggleClass("icon-chevron-down");
                        e.preventDefault();});
                }else{
                    this.template = _.template( '<span><i class="icon-file"></i><a href="#"><%= model.name%></a></span>', { model:_.extend(this.model.toJSON(),{id:escapedId}) } );
                    $(this.el).html(this.template);
                }
                return this;
            }
        });

        var TreeRoot = Backbone.View.extend({
            tagName: "ul",
            className:"collapse in nav nav-list",
            render:function(){
                var el=$(this.el);
                el.attr("id",this.model.id);
                this.model.bundles.each(function(node){
                    el.append(new TreeView({model:node}).render().el);
                });
                return this;
            }
        });

        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
            },
            render: function(){
                this.template = _.template( template, { /*model: this.model.toJSON()*/ } );
                $(this.el).html(this.template);
                $(this.el).find("#confTree").append(new TreeRoot({model:this.model}).render().el);

              //  $(this.el).find("#confTree li").collapse();
                return this;
            },
            'dispose':function(){

            }
        });

        return View;
    });
