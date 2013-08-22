define([
    'jquery',
    'backbone',
    'underscore',
    'views/instanceRow',
    'text!templates/instancesList.html',
    'views/confirm',
    'views/commandPopup',
    'views/alerts'],
    function($, Backbone, _,instanceView, template,confirmView,commandPopupView,alerts){
        var View = Backbone.View.extend({
            el: '#main',
            initialize: function(){
                this.filter=this.options.filter;
                _(this).bindAll('add', 'remove','reset','destroy','start','stop','dispose','command');
                this.instances=this.options.instances;
                this.subViews=[];
                var self=this;
                var filtered;
                if(this.filter)
                    filtered=this.instances.select(function(i){return i.get("ApplicationId")==self.filter.ApplicationId && i.get("ApplicationVendor")==self.filter.ApplicationVendor ;});
                else
                    filtered=this.instances.models;
                _.each(filtered,function(instance){
                    self.add(instance);
                });
                this.instances.bind('add', this.add);
                this.instances.bind('remove', this.remove);
                this.instances.bind('reset', this.reset);
                this.rendered=false;
            },
            add : function(instance) {
                if(this.filter && (instance.get("ApplicationId")!=this.filter.ApplicationId || instance.get("ApplicationVendor")!=this.filter.ApplicationVendor))
                    return;
                var view = new instanceView({
                    tagName : 'tr',
                    model : instance
                });
                view.bind("destroy",this.destroy);
                view.bind("start",this.start);
                view.bind("stop",this.stop);
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
                if(this.filter && (instance.get("ApplicationId")!=this.filter.ApplicationId || instance.get("ApplicationVendor")!=this.filter.ApplicationVendor))
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
            render: function(){
                this.template = _.template( template, {  } );
                $(this.el).empty();
                $(this.el).append(this.template);
                _(this.subViews).each(function(v) {
                    this.$('.instances tbody').append(v.render().el);
                });

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

                var cmd=_.find(  model.attributes.Commands ,function(c){ return c.Name == command; });
                commandPopupView.open(cmd)
                    .done(function(){
                        model.command(commandPopupView.command,{
                            success: function (model,data){
                                if(typeof data.Message!='undefined')
                                    alerts.show({type:"info",text:data.Message});
                            },
                            error:function(model,response){
                                view.render();
                                alerts.show({type:"error",text:"Failed to execute command '"+commandPopupView.command.Name+"' for  instance '"+model.id+"'.  "+JSON.parse(response.responseText).Error});
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
                this.removeViews(this.subViews)
            }
        });

        return View;
    });
