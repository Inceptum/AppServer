define([
    'jquery',
    'backbone',
    'underscore',
    //'models/model',
    'text!templates/header.html','context','signalr','signalrHubs'],
    function($, Backbone, _, template, context){
        var View = Backbone.View.extend({
            el: '#header',
            initialize: function () {
                _(this).bindAll('onHeartBeat');
                this.template = _.template( template, { } );
                this.notifications = $.connection.uiNotificationHub;
                $.connection.hub.url = context.signalRUrl('/signalr');
                this.notifications.HeartBeat=this.onHeartBeat;
                this.blinking=false;
            },
            onHeartBeat:function(){
                if(this.blinking)
                    return;
                this.blinking=true;
                var self=this;
                this.led.fadeTo(100,1,function(){
                    self.led.fadeTo(300,.25);
                    self.blinking=false;
                });
            },
            render: function () {
                $(this.el).html(this.template);
                this.led=$(this.el).find('.led').fadeTo(10,.25);
                $.connection.hub.start();
                this.rendered=true;
            },

            selectMenuItem: function (menuItem) {
                $('.topMenu li').removeClass('active');
                if (menuItem) {
                    $('.topMenu li.' + menuItem).addClass('active');
                }
            },
            'dispose':function(){
                if(this.rendered)
                    $.connection.hub.stop()
            }
        });

        return new View();
    });