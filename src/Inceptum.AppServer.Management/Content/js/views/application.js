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
                _(this).bindAll('remove','change');
                this.model.bind('remove', this.remove);
                this.model.bind('change', this.change);
            },
            remove:function(model){
                alerts.show({
                    type:"warning",
                    text:"Application '"+model.id+"' was removed"
                });
                this.navigate("#applications");
            },
            change:function(model){
                this.instancesList.dispose();
                this.render();
                //this.navigate("#applications/"+model.id);
            },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find('.app-versions ul li a').first().click();
                this.instancesList = new InstancesListView({ el: $(this.el).find(".instances"), instances:this.instances, filter:{ApplicationId:this.model.get("Name"),ApplicationVendor:this.model.get("Vendor")}});
                this.instancesList.render();
            },
            'dispose':function(){
                if(this.instancesList )
                    this.instancesList.close();
            }
        });

        return View;
    });
