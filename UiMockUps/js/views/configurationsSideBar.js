define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/configurationsSideBar.html',
    'collections/configurations'],
    function($, Backbone, _, template,Configurations){
        var View = Backbone.View.extend({
            el:'#sidebar',
            initialize: function(options){
                this.activeItem=options.active;
                this.template = _.template( template, { model: Configurations.toJSON() } );
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
/**
 * Created with JetBrains WebStorm.
 * User: knst
 * Date: 07.10.12
 * Time: 16:08
 * To change this template use File | Settings | File Templates.
 */
