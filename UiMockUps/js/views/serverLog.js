define([
    'jquery',
    'backbone',
    'underscore',
    'context',
    'views/serverSideBar',
    'collections/instances',
    'text!templates/serverLog.html',
    'context',
    'scrollTo',
    'noext!sr/signalr/hubs'],
    function($, Backbone, _,context,ServerSideBarView,instances, template,context){
        var View = Backbone.View.extend({
            el:'#content',
            levelMap:{
                "Fatal":"text-error",
                "Error":"text-error",
                "Warn":"text-warning",
                "Info":"text-info",
                "Debug":"muted"
            },
            initialize: function(){
                _.bindAll(this, "onMessageReceived","onReconnected","applyFilter");
                var self=this;
                this.connection = $.connection(context.signalRUrl('/log'));
                this.connection.received(this.onMessageReceived);
                this.connection.reconnected(this.onReconnected);
                this.connection.error(function(error) {
                    console.log(error);
                });
                this.connection.stateChanged(function (change) {
                    if (change.oldState == $.signalR.connectionState.connected ||  change.newState === $.signalR.connectionState.disconnected) {
                        self.connectionStateLabel.show();
                    }
                    if (change.newState === $.signalR.connectionState.reconnecting) {
                        console.log('Re-connecting');
                    }
                    else if (change.newState === $.signalR.connectionState.connected) {
                        console.log('connected');
                        self.connectionStateLabel.hide();
                    }
                    else if (change.newState === $.signalR.connectionState.disconnected) {
                        console.log('disconnected');
                    }
                    else if (change.newState === $.signalR.connectionState.connecting) {
                        console.log('connecting');
                    }
                });

                this.template = _.template( template, { model: {} } );
            },
            events:{
                "change #filter": "applyFilter"
            },
            applyFilter:function(){
                if(this.filter.val()==="All")
                   this.log.find("p").show();
                else{
                   this.log.find("p").hide();
                   this.log.find('p[data-source="'+this.filter.val()+'"]').show();
                }
            },
            onReconnected:function() {
                this.log.html("");
            },
            onMessageReceived :function (data) {
                var needToScroll=$(".log p").length>0 && $(".log p").last().offset().top - $(".log").offset().top<=$(".log").height();
                var p;
                var self=this;
                if( Object.prototype.toString.call( data ) === '[object Array]' ) {
                    _.each(data.reverse(),function(message){
                        p = self.createLogItem(message);
                        self.log.prepend(p);
                    });
                    p=this.log.find("p").last();
                    needToScroll=true;
                }else{
                    p = this.createLogItem(data);
                    this.log.append(p);
                }
                if(needToScroll)
                    this.log.scrollTo(p);
            },
            createLogItem:function(logEvent){
                var p = $('<p data-source="'+logEvent.source+'">');
                if(this.filter.val()==="All" || this.filter.val()===logEvent.source)
                    p.show();
                else
                    p.hide();
                if(logEvent.level=="Fatal"){
                    p.prepend($('<span class="label label-important">'+logEvent.message+'</span>'));
                }else{
                    p.addClass(this.levelMap[logEvent.level]).html(logEvent.message);
                }
                return p;
            },
            render: function(){
                $(this.el).html(this.template);
                this.log = $(this.el).find(".log");
                this.title = $(this.el).find(".title");
                this.filter = $(this.el).find('#filter');
                var self=this;
                //TODO: update select on instances change
                instances.each(function(i){
                    self.filter.append($("<option></option>").text(i.id));
                });
                this.connection.start({
                    //SignalR is loaded via requireJs. In IE window load event is already fired at connection start. Thus signalr would wait forever if waitForPageLoad is true
                    waitForPageLoad: false
                });
                this.connectionStateLabel= $(this.el).find(".connectionState").hide();
            },
            'dispose':function(){
                this.connection.stop();
            }

        });

        return View;
    });
