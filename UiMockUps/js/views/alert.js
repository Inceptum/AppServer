define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/alert.html'],
    function($, Backbone, _,  template){
        var View = Backbone.View.extend({
            el:".alerts",
            show: function(options){
                var params= _.extend( {
                    type: 'error',
                    text: ''
                },options);
                params=_.extend(params,{typeText:params.type[0].toUpperCase()+params.type.substr(1)});
                var deferred = this.deferred = new $.Deferred;
                this.template = _.template( template, params );
                $(this.el).html(this.template);
                return deferred;
            }
        });

        return View;
    });
