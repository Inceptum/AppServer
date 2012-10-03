define([
    'jquery',
    'backbone',
    'underscore',
    'views/serverSideBar',
    'views/instancesList',
    'text!templates/serverStatus.html'],
    function($, Backbone, _, ServerSideBarView,InstancesListView, template){
        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                this.instances=this.options.instances;
            },
            render: function(){
            this.template = _.template( template, { model: this.model.toJSON() } );

                $(this.el).html(this.template);
                this.instancesList = new InstancesListView({ el: $(this.el).find(".instances"), instances:this.instances});
                this.instancesList.render();
                this.rendered = true;
                return this;
            },
            'dispose':function(){
                if(this.instancesList )
                    this.instancesList.close();

            }
        });

        return View;
    });
