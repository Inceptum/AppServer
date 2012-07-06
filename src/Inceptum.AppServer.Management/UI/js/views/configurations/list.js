define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/configurations/list.html'
], function ($, _, Backbone, configurationListTemplate) {
    var configurationListView = Backbone.View.extend({

        template:_.template(configurationListTemplate),

        events:{
            "click #update":"update",
            "click #new":"addConfig",
            "click a.delete":"deleteConfig"
        },

        initialize:function () {
            this.collection.bind("reset add change destroy", this.render, this);
        },

        beforeClose:function () {
            this.collection.unbind(null, null, this);
        },

        update:function () {
            this.collection.fetch();
        },

        addConfig:function () {
            var cfgName = window.prompt("Enter new configuration name: ");
            if (cfgName && cfgName.trim() != '') {
                var view = this;
                this.collection.create({ name:cfgName.trim() }, {
                    error:function (originalModel, resp) {
                        Router.showMessage('warning', 'Error creating configuration ' + originalModel.get("name") + ": " + resp.responseText);
                        view.update();
                    }
                });
            }
        },

        deleteConfig:function (e) {
            e.stopImmediatePropagation();

            var cfgName = $(e.target).attr("data-cfg-id"),
                cfg = this.collection.get(cfgName),
                view = this;

            if (window.confirm("Do you REALLY want to DELETE " + cfg.id + "?")) {
                cfg.destroy({
                    success:function () {
                        Router.showMessage('success', 'Configuration ' + cfg.id + ' was successfully deleted');
                    },
                    error:function (originalModel, resp) {
                        Router.showMessage('warning', 'Error deleting configuration ' + originalModel.id + ": " + resp.responseText);
                        view.update();
                    }
                });
            }
            return false;
        },

        render:function () {
            var data = {
                configurations:this.collection.models,
                _:_
            };
            this.$el.html(this.template(data));
            return this;
        }

    });

    return configurationListView;
});
