define([
    'jquery',
    'backbone',
    'underscore',
    'views/instancesList',
    'views/alerts',
    'text!templates/application.html'],
    function($, Backbone, _,InstancesListView,alerts, template){
        var View = Backbone.View.extend({
            el: '#content',
            events: {
                "click #more":"toggleVersions"
            },
            initialize: function () {
                
                this.instances=this.options.instances;
                _(this).bindAll('remove', 'change', 'toggleVersions');
                this.model.bind('remove', this.remove);
                this.model.bind('change', this.change);
            },
            toggleVersions: function (e) {
                $(this.el).find(".oldVersion").toggleClass("hide");
                if ($(this.el).find("#more").text() == 'more...')
                    $(this.el).find("#more").text('less...');
                else
                    $(this.el).find("#more").text('more...');
                e.preventDefault();
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
                this.instancesList = new InstancesListView({ el: $(this.el).find(".instances"), instances:this.instances, filter:{applicationId:this.model.get("name"),applicationVendor:this.model.get("vendor")}});
                this.instancesList.render();
            },
            'dispose':function(){
                if(this.instancesList )
                    this.instancesList.close();
            }
        });

        return View;
    });
