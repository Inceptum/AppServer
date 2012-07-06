define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/configurations/configuration.html',
    'order!libs/jquery/jsTree/jquery.jstree'
], function ($, _, Backbone, configurationTemplate) {
    var configurationView = Backbone.View.extend({

        template:_.template(configurationTemplate),

        events:{
            "click .new":"newBundle"
        },

        initialize:function () {
            _.bindAll(this, "render");
            this.model.bind("change", this.render, this);
        },

        beforeClose: function() {
          this.model.unbind(null,null,this);
        },

        newBundle:function () {
            var bundleName = window.prompt("Enter child bundle name: ");
            if (!bundleName || bundleName.trim() === "") return;
            Router.createBundle(bundleName.trim().toLowerCase());
        },

        render:function () {
            var data = {
                configuration:this.model,
                _:_
            };
            var self = this;
            this.$el.html(this.template(data));
            this.$el.find(".bundles").jstree({
                core:{initially_open:true},
                plugins:["themes", "json_data", "ui"  ],
                json_data:{data:this.model.get("bundlesmap")}
            }).bind("select_node.jstree",
                function (e, data) {
                    self.$el.find(".new").text("New child bundle");
                    if ($.data(data.rslt.obj[0], "href")) {
                        window.location.hash = $.data(data.rslt.obj[0], "href");
                    }
                }).bind("before.jstree", function (e, data) {
                    if (data.func === "open_node" || data.func === "close_node") {
                        e.stopImmediatePropagation();
                        return false;
                    }
                });
            return this;
        }
    });
    return configurationView;
});

