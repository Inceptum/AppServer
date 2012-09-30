define(['jquery','underscore','collections/applicationInstances','context','signalrHubs'],
    function(jQuery,_,applicationInstances,context){
    var listener={};
    _.extend(listener,{
        init:function(){
            applicationInstances.fetch({async:false});
            this.initSignalR();
        },
        initSignalR:function(){
            this.notifications = $.connection.uiNotificationHub;
            $.connection.hub.url = context.signalRUrl('/signalr');
            var self=this;
            this.notifications.InstancesChanged=function(comment){
                console.log("Scheduling fetch due to "+comment);
                self.fetch(comment);
            }
            $.connection.hub.start();
        },
        fetch:function(comment){
            var self=this;
            if(this.isFetching){
                self.needToFetch=true;
                return;
            }
            self.needToFetch=false;
            this.isFetching=true;
            applicationInstances.fetch({
                success:function(){
                    self.isFetching=false;
                    if(self.needToFetch)
                        self.fetch();
                }
            });
        },
        dispose:function(){
            $.connection.hub.stop();
        }
    });

    return listener;
});
