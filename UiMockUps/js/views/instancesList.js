define([
    'jquery',
    'backbone',
    'underscore',
    'views/instanceRow',
    'text!templates/instancesList.html',
    'text!templates/error.html',
    'views/confirm'
],
    function($, Backbone, _,instanceView, template,errorTemplate,confirmView){
        var View = Backbone.View.extend({
            el: '#main',
            initialize: function(){
                this.filter=this.options.filter;
                _(this).bindAll('add', 'remove','reset','destroy','start','stop');
                this.instances=this.options.instances;
                this.subViews=[];
                var self=this;
                var filtered;
                if(this.filter)
                    filtered=this.instances.select(function(i){return i.get("ApplicationId")==self.filter;});
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
            events:{
                "click .reload":"reload"
            },
            reload:function(){
                if(this.instances.length >0)
                    this.instances.remove(this.instances.models[0]);
                else
                    this.instances.fetch();
            },
            add : function(instance) {
                if(this.filter && instance.get("ApplicationId")!=this.filter)
                    return;
                var view = new instanceView({
                    tagName : 'tr',
                    model : instance
                });
                view.bind("destroy",this.destroy);
                view.bind("start",this.start);
                view.bind("stop",this.stop);


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
                if(this.filter && instance.get("ApplicationId")!=this.filter)
                    return;
                var viewToRemove = _(this.subViews).select(function(v) { return v.model === instance; })[0];
                this.subViews = _(this.subViews).without(viewToRemove);
                if (this.rendered) {
                    $(viewToRemove.el).remove();
                    viewToRemove.close();
                    viewToRemove.unbind("destroy",this.destroy);
                    viewToRemove.unbind("start",this.start);
                    viewToRemove.unbind("stop",this.stop);
                }

            },
            reset : function(model) {
                _(this.subViews).each(function(v) {
                    $(v.el).remove();
                    v.close();
                });
                this.subViews=[];
                this.instances.each(this.add);
            },
            render: function(){
                this.template = _.template( template, {  } );
                $(this.el).empty();
                $(this.el).append(this.template);
                this.alerts=this.$(".alerts");

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
                confirmView.open({title:"Delete",body:"You are bout to delete '"+model.id+"' instance. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        model.destroy({
                             wait: true,
                             error:function(model,response){
                                 view.render();
                                 self.alerts.empty().append(_.template( errorTemplate, { model:JSON.parse(response.responseText) }));
                             }});
                    }).fail(view.render());
            },
            start:function(model,view){
                var self=this;
                model.start({
                    error:function(model,response){
                        view.render();
                        self.alerts.empty().append(_.template( errorTemplate, { model:JSON.parse(response.responseText) }));
                    }});
            },
            stop:function(model,view){
                var self=this;
                model.stop({
                    error:function(model,response){
                        view.render();
                        self.alerts.empty().append(_.template( errorTemplate, { model:JSON.parse(response.responseText) }));
                    }});
            },
            'dispose':function(){
                this.instances.unbind('add', this.add);
                this.instances.unbind('remove', this.remove);
                this.instances.unbind('reset', this.reset);
                _.each(this.subViews,function(subView){
                    subView.close();
                    subView.unbind("destroy",this.destroy);
                    subView.unbind("start",this.start);
                    subView.unbind("stop",this.stop);
                });
            }
        });

        return View;
    });
