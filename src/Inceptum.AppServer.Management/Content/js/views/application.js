define([
    'jquery',
    'backbone',
    'underscore',
    'views/instancesList',
    'views/alerts',
    'text!templates/application.html'],
    function($, Backbone, _,InstancesListView,alerts, template){
        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                this.instances=this.options.instances;
                _(this).bindAll('remove');
                this.model.bind('remove', this.remove);
            },
            remove:function(model){
                alerts.show({
                    type:"warning",
                    text:"Application '"+model.id+"' was removed"
                });
                this.navigate("#applications");
            },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find('.app-versions ul li a').first().click();
                this.instancesList = new InstancesListView({ el: $(this.el).find(".instances"), instances:this.instances, filter:this.model.get("Name")});
                this.instancesList.render();
            },
            'dispose':function(){
                if(this.instancesList )
                    this.instancesList.close();
            }
        });

        return View;
    });
