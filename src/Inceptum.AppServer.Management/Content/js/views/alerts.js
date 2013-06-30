define([
    'jquery',
    'backbone',
    'underscore',
    'libs/bootstrap-notify'],
    function($, Backbone, _){
        var View = Backbone.View.extend({
            attributes:{"style":"width:300px;z-index: 10000"},
            className: "notifications top-right",
            show: function(options){
                var self=this;
                var params= _.extend( {type: 'error',text: ''},options);
                $('body').append(this.el);


                var alert=$(this.el).notify({
                    type: params.type,
                    message: {text:params.text},
                    fadeOut: { enabled: params.fadeOut==true, delay:3000}
                });
                alert.show();
            }
        });
        return new View();
    });
