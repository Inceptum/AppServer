define([
    'jquery',
    'backbone',
    'underscore',
    'views/instanceRow',
    'text!templates/instancesList.html',
    'views/confirm',
    'views/alerts'],
    function($, Backbone, _,instanceView, template,confirmView,alerts){
        var View = Backbone.View.extend({
            el: '#main',
            initialize: function(){
                this.filter=this.options.filter;
                _(this).bindAll('add', 'remove','reset','destroy','start','stop','dispose');
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
                    this.removeViews(viewToRemove);
                }

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
            'dispose':function(){
                this.instances.unbind('add', this.add);
                this.instances.unbind('remove', this.remove);
                this.instances.unbind('reset', this.reset);
                this.removeViews(this.subViews)
            }
        });

        return View;
    });
