define([
    'jquery',
    'backbone',
    'underscore',
    'context',
    'views/serverSideBar',
    'collections/instances',
    'text!templates/serverLog.html',
    'noext!signalr/hubs'],
    function($, Backbone, _,context,ServerSideBarView,instances, template){
        var View = Backbone.View.extend({
            el:'#content',
            levelMap:{
                "Fatal":"text-error",
                "Error":"text-error",
                "Warn":"text-warning",
                "Info":"text-info",
                "Debug":"muted"
            },
            initialize: function (options) {
                this.selectedInstance = options.selectedInstance;
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
                        console.log('logs: re-connecting');
                    }
                    else if (change.newState === $.signalR.connectionState.connected) {
                        console.log('logs: connected');
                        self.connectionStateLabel.hide();
                    }
                    else if (change.newState === $.signalR.connectionState.disconnected) {
                        console.log('logs: disconnected');
                    }
                    else if (change.newState === $.signalR.connectionState.connecting) {
                        console.log('logs: connecting');
                    }
                });

                this.template = _.template( template, { model: {} } );
            },
            events:{
                "change #filter": "applyFilter"
            },
            applyFilter: function () {
                if (this.filter.val() === "All")
                    this.log.find("p").show();
                else {
                    this.log.find("p").hide();
                    this.log.find('p[data-source="' + this.filter.val() + '"]').show();
                }
            },
            onReconnected:function() {
                this.log.html("");
            },
            onMessageReceived :function (data) {
                var p;
                var self = this;

                if (Object.prototype.toString.call(data) === '[object Array]') {
                    _.each(data, function (message) {
                        p = self.createLogItem(message);
                        self.log.prepend(p);
                    });
                    p = this.log.find("p").last();
                } else {
                    p = this.createLogItem(data);
                    this.log.prepend(p);
                }
            },
            createLogItem:function(logEvent){
                var p = $('<p data-source="'+logEvent.source+'">');
                if(this.filter.val()==="All" || this.filter.val()===logEvent.source)
                    p.show();
                else
                    p.hide();
                var message = logEvent.message;
                if(logEvent.level=="Fatal"){
                    p.prepend($('<span class="label label-important"></span>').text(message));
                }else{
                    p.addClass(this.levelMap[logEvent.level]).text(message);
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
                }).done();
                this.connectionStateLabel = $(this.el).find(".connectionState").hide();

                if (this.selectedInstance) {
                    this.filter.val(this.selectedInstance);
                }
            },
            'dispose':function(){
                this.connection.stop();
            }

        });

        return View;
    });
