define(['jquery','underscore','collections/instances','collections/applications','context','noext!signalr/hubs','throttle'],
    function(jQuery,_,instances,applications,context){
    var listener={};
    _.extend(listener,{
        init:function(){
            instances.fetch({async:false});
            applications.fetch({async:false});
            this.initSignalR();
            this.fetchInstances=$.throttle(500,this.fetchInstances);
            this.fetchApplications=$.throttle(500,this.fetchApplications);
        },
        initSignalR:function(){
            this.notifications = $.connection.uiNotificationHub;
            $.connection.hub.url = context.signalRUrl('/signalr');
            var self=this;
            this.notifications.client.InstancesChanged=function(comment){
                console.log("Scheduling fetch due to "+comment);
                self.fetchInstances(comment);
            }
            this.notifications.client.ApplicationsChanged=function(comment){
                console.log("Scheduling fetch applications due to "+comment);
                self.fetchApplications(comment);
            }
            $.connection.hub.start({
                //SignalR is loaded via requireJs. In IE window load event is already fired at connection start. Thus signalr would wait forever if waitForPageLoad is true
                waitForPageLoad: false
            });
            this.cnt=0;
        },
        fetchInstances:function(comment){
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
                        self.fetchInstances();
                },
                error:function(){
                    console.log("fetch #"+cnt+ " failed")
                    self.fetchInstances();
                }
            });
            this.cnt++;
        },
        fetchApplications:function(comment){
            var self=this;
            if(this.isApplicationsFetching){
                self.needToFetchApplications=true;
                return;
            }
            this.isApplicationsFetching=true;
            self.needToFetchApplications=false;
            console.log("apps fetch #"+this.cnt+ " start");
            var cnt=this.cnt;

            applications.fetch({
                removeMissing:true,
                update:true,
                success:function(){
                    console.log("apps fetch #"+cnt+ " complete")
                    self.isApplicationsFetching=false;
                    if(self.needToFetchApplications)
                        self.fetchApplications();
                },
                error:function(){
                    console.log("apps fetch #"+cnt+ " failed")
                    self.fetchApplications();
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
