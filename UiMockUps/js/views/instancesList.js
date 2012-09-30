define([
    'jquery',
    'backbone',
    'underscore',
    'views/applicationInstanceRow',
    'text!templates/instancesList.html'],
    function($, Backbone, _,ApplicationInstanceView, template){
        var View = Backbone.View.extend({
            el: '#main',
            initialize: function(){
                this.filter=this.options.filter;
                _(this).bindAll('add', 'remove','reset');
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
                var view = new ApplicationInstanceView({
                    tagName : 'tr',
                    model : instance
                });

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

                _(this.subViews).each(function(v) {
                    this.$('.instances tbody').append(v.render().el);
                });

                $.connection.hub.start();
                // We keep track of the rendered state of the view
                this.rendered = true;
                return this;
            },
            'dispose':function(){
                this.instances.unbind('add', this.add);
                this.instances.unbind('remove', this.remove);
                this.instances.unbind('reset', this.reset);
                _.each(this.subViews,function(subView){
                    subView.close();
                });
            }
        });

        return View;
    });
