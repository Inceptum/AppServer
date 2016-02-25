define([
    'jquery',
    'backbone',
    'underscore',
    'views/instanceRow',
    'text!templates/instancesList.html',
    'views/confirm',
    'views/commandPopup',
    'views/alerts'],
    function ($, Backbone, _, instanceView, template, confirmView, commandPopupView, alerts) {

        var store = function(key, value) {
            if (sessionStorage && sessionStorage.setItem) {
                sessionStorage.setItem('Inceptum.AppServer.' + key, JSON.stringify(value));
            }
        };
        var restore = function (key) {
            if (sessionStorage && sessionStorage.getItem) {
                var val = sessionStorage.getItem('Inceptum.AppServer.' + key);
                if (val) {
                    try {
                        return JSON.parse(val);
                    } catch (e) {

                    } 
                }
            }
            return undefined;
        };

        var View = Backbone.View.extend({
            el: '#main',


            initialize: function () {
                this.nameFilter = null;
                this.filter=this.options.filter;
                _(this).bindAll('add', 'remove','reset','destroy','start','stop','kill','dispose','command');
                this.instances=this.options.instances;
                this.subViews=[];
                var self=this;
                var filtered;
                if(this.filter)
                    filtered=this.instances.select(function(i){return i.get("applicationId")==self.filter.applicationId && i.get("applicationVendor")==self.filter.applicationVendor ;});
                else
                    filtered=this.instances.models;
                _.each(filtered,function(instance){
                    self.add(instance);
                });
                this.instances.bind('add', this.reset);
                this.instances.bind('remove', this.reset);
                this.instances.bind('reset', this.reset);
                this.rendered=false;
            },
            events: {
                "keyup #nameFilter": "applyNameFilter",
                "click .batch-buttons .start":"batchStart",
                "click .batch-buttons .stop":"batchStop",
                "click .batch-buttons .restart":"batchRestart",
                },

            batchStop:function(e){
                _(this.subViews).each(function(v) {
                    v.stop(e);
                });
            },
            batchStart:function(e){
                _(this.subViews).each(function(v) {
                    v.start(e);
                });
            },
            batchRestart:function(e){
                _(this.subViews).each(function(v) {
                    v.restart(e);
                });
            },


            applyNameFilter: function () {
                store('InstancesList.NameFilter', this.nameFilter.val());
                this.reset();
            },
            add : function(instance) {
                if(this.filter && (instance.get("applicationId")!=this.filter.applicationId || instance.get("applicationVendor")!=this.filter.applicationVendor))
                    return;
                if (this.nameFilter!=null && this.nameFilter.val() != '' && 
                    (instance.get('name').toLowerCase().indexOf(this.nameFilter.val().toLowerCase()) == -1 &&
                    instance.get('environment').toLowerCase().indexOf(this.nameFilter.val().toLowerCase()) == -1 &&
                    instance.get('applicationId').toLowerCase().indexOf(this.nameFilter.val().toLowerCase()) == -1 &&
                    instance.get('status').toLowerCase().indexOf(this.nameFilter.val().toLowerCase()) == -1 ))
                    return;

                var view = new instanceView({
                    tagName : 'tr',
                    model : instance
                });
                view.bind("destroy",this.destroy);
                view.bind("start",this.start);
                view.bind("debug",this.debug);
                view.bind("restart",this.restart);
                view.bind("stop",this.stop);
                view.bind("kill",this.kill);
                view.bind("command",this.command);


                //TODO: sort order id not preserved
                // And add it to the collection so that it's easy to reuse.
                this.subViews.push(view);

                // If the view has been rendered, then
                // we immediately append the rendered view.
                if (this.rendered) {
                    this.$('.instances tbody').append(view.render().el);
                }
            },
            remove : function(instance) {
                if(this.filter && (instance.get("applicationId")!=this.filter.applicationId || instance.get("applicationVendor")!=this.filter.applicationVendor))
                    return;
                var viewToRemove = _(this.subViews).select(function(v) { return v.model === instance; })[0];
                this.subViews = _(this.subViews).without(viewToRemove);
                if (this.rendered) {
                    $(viewToRemove.el).remove();
                    this.removeViews(viewToRemove);
                }
                console.log('remove');
            },
            removeViews:function(views){
                if(!( Object.prototype.toString.call( views ) === '[object Array]' )){
                    views=[views];
                }
                var self=this;
                _.each(views,function(v){
                    v.unbind("destroy",self.destroy);
                    v.unbind("start",self.start);
                    v.unbind("stop",self.stop);
                    v.unbind("command",self.command);
                    v.close();
                });
            },
            reset : function(model) {
                _(this.subViews).each(function(v) {
                    $(v.el).remove();
                    v.close();
                });
                this.subViews=[];
                this.instances.each(this.add);
                console.log('reset');
            },
            render: function () {
                var that = this;

                this.template = _.template(template, {});
                $(this.el).empty();
                $(this.el).append(this.template);
                _(this.subViews).each(function(v) {
                    this.$('.instances tbody').append(v.render().el);
                });

                this.nameFilter = $(this.el).find('#nameFilter');
                this.nameFilter.val(restore('InstancesList.NameFilter'));
                setTimeout(function() {
                    if (that.nameFilter.val()) {
                        that.applyNameFilter();
                    }
                }, 0);

                $.connection.hub.start();
                // We keep track of the rendered state of the view
                this.rendered = true;

                return this;
            },
            destroy:function(model,view){
                var self=this;
                confirmView.open({title:"Delete",body:"You are about to delete '"+model.id+"' instance. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        model.destroy({
                             wait: true,
                             error:function(model,response){
                                 view.render();
                                 alerts.show({type:"error",text:"Failed to delete instance '"+model.id+"'. "+JSON.parse(response.responseText).Error});
                             }});
                    }).fail(view.render());
            },
            start:function(model,view){
                var self=this;
                model.start({
                    error:function(model,response){
                        view.render();
                        alerts.show({type:"error",text:"Failed to start instance '"+model.id+"'. "+JSON.parse(response.responseText).Error});
                    }
                });
            },
            debug: function (model, view) {
                var self=this;
                model.debug({
                    error:function(model,response){
                        view.render();
                        alerts.show({type:"error",text:"Failed to start instance '"+model.id+"'. "+JSON.parse(response.responseText).Error});
                    }
                });
            },
            kill: function (model, view) {
               var self = this;
               confirmView.open({ title: "Kill process", body: "You are about to kill '" + model.id + "' instance process. Are you sure?", confirm_text: "Kill" })
                   .done(function () {
                       model.kill({
                           error: function (model, response) {
                               view.render();
                               alerts.show({ type: "error", text: "Failed to kill instance '" + model.id + "' process. " + JSON.parse(response.responseText).Error });
                           }
                       });
                   }).fail(view.render());
                },
            restart:function(model,view){
                var self=this;
                model.restart({
                    error:function(model,response){
                        view.render();
                        alerts.show({type:"error",text:"Failed to restart instance '"+model.id+"'. "+JSON.parse(response.responseText).Error});
                    }
                });
            },
            stop:function(model,view){
                var self=this;
                model.stop({
                    error:function(model,response){
                        view.render();
                        alerts.show({type:"error",text:"Failed to stop instance '"+model.id+"'.  "+JSON.parse(response.responseText).Error});
                    }
                });
            },
            command:function(model,view,command){
                var self=this;

                var cmd=_.find(  model.attributes.commands ,function(c){ return c.name == command; });
                commandPopupView.open(cmd)
                    .done(function(){
                        model.command(commandPopupView.command,{
                            success: function (model,data){
                                if(typeof data.message!='undefined')
                                    alerts.show({type:"info",text:data.message});
                            },
                            error:function(model,response){
                                view.render();
                                alerts.show({type:"error",text:"Failed to execute command '"+commandPopupView.command.name+"' for  instance '"+model.id+"'.  "+JSON.parse(response.responseText).Error});
                            }     ,
                            complete :function(){
                                view.render();
                            }
                        });
                    }).fail(view.render());
            },
            'dispose':function(){
                this.instances.unbind('add', this.add);
                this.instances.unbind('remove', this.remove);
                this.instances.unbind('reset', this.reset);
                this.removeViews(this.subViews);
            }
        });

        return View;
    });
