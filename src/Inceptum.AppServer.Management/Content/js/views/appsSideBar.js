define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/appsSideBar.html'],
    function($, Backbone, _, template){
        var View = Backbone.View.extend({
            el:'#sidebar',
            initialize: function(options){
                this.activeItem=options.active;
                this.template = _.template( template, { model: this.collection.toJSON() } );
            },
            render: function(){
                $(this.el).html(this.template);
                $(this.el).find('*[data-id="'+this.activeItem+'"]').addClass("active");
            },
            'dispose':function(){
            }
        });

        return View;
    });
