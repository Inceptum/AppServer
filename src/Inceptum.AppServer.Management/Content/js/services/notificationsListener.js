define(['jquery','underscore','collections/instances','context','noext!sr/signalr/hubs','throttle'],
    function(jQuery,_,instances,context){
    var listener={};
    _.extend(listener,{
        init:function(){
            instances.fetch({async:false});
            this.initSignalR();
            this.fetch=$.throttle(500,this.fetch);
        },
        initSignalR:function(){
            this.notifications = $.connection.uiNotificationHub;
            $.connection.hub.url = context.signalRUrl('/signalr');
            var self=this;
            this.notifications.InstancesChanged=function(comment){
                console.log("Scheduling fetch due to "+comment);
                self.fetch(comment);
            }
            $.connection.hub.start({
                //SignalR is loaded via requireJs. In IE window load event is already fired at connection start. Thus signalr would wait forever if waitForPageLoad is true
                waitForPageLoad: false
            });
            this.cnt=0;
        },
        fetch:function(comment){
            var self=this;
            if(this.isFetching){
                self.needToFetch=true;
                return;
            }
            this.isFetching=true;
            self.needToFetch=false;
            console.log("fetch #"+this.cnt+ " start");
            var cnt=this.cnt;

            instances.fetch({
                removeMissing:true,
                update:true,
                success:function(){
                    console.log("fetch #"+cnt+ " complete")
                    self.isFetching=false;
                    if(self.needToFetch)
                        self.fetch();
                },
                error:function(){
                    console.log("fetch #"+cnt+ " failed")
                    self.fetch();
                }
            });
            this.cnt++;
        },
        dispose:function(){
            $.connection.hub.stop();
        }
    });

    return listener;
});
