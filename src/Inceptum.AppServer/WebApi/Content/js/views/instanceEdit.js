define([
        'jquery',
        'backbone',
        'underscore',
        'models/instance',
        'collections/configurations',
        'text!templates/instanceEdit.html',
        'views/alerts', 'spinedit'
    ],
    function($, Backbone, _, instanceModel, configurations, template, alerts) {
        var View = Backbone.View.extend({
            el: '#content',
            initialize: function() {
                _(this).bindAll('submit', 'reset', 'render');
                this.application = this.options.application;
                this.activeItem = this.options.active;

                if (!this.model)
                    this.model = new instanceModel({ "applicationId": this.application.attributes.name, "applicationVendor": this.application.attributes.vendor });
                else
                    this.model = this.model.clone();
                this.model.bind('change', this.reset);
                this.application.bind('change', this.reset);
            },
            events: {
                "change": "change",
                "click #submit": "submit"
            },
            reset: function(model) {
                this.render();
            },
            render: function() {
                this.template = _.template(template, { model: this.model.toJSON() });
                $(this.el).html(this.template);

                var action = this.model.isNew() ? "Create" : "Save";
                this.$("#submit strong").text(action);
                var versionSelect = $(this.el).find("#inputVersion");
                var self = this;
                this.application.versions.each(function(version) {
                    var option = $("<option></option>");
                    if (self.model.get("version") === version.id)
                        option.attr("selected", "selected");
                    option.text(version.id).attr("value", version.id).appendTo(versionSelect);
                });

                var defaultConfigurationSelect = $(this.el).find("#defaultConfiguration");

                $('#inputStartOrder').spinedit({
                    minimum: 0,
                    maximum: 2147483647,
                    step: 1
                });


                configurations.each(function(config) {
                    var option = $("<option></option>");
                    if (self.model.get("defaultConfiguration") === config.id)
                        option.attr("selected", "selected");
                    option.text(config.id).attr("value", config.id).appendTo(defaultConfigurationSelect);
                });

            },
            'submit': function(e) {
                e.preventDefault();
                var self = this;
                var action = this.model.isNew() ? "create" : "update";

                this.model.save(null, {
                    success: function(model) {
                        alerts.show({ type: "info", text: "Instance '" + model.get("name") + "' " + action + "d" });
                        self.navigate('#/applications/' + model.get("applicationVendor") + '/' + model.get("applicationId"), true);
                    },
                    error: function(model, response) {
                        var instanceId = action === "update" ? "'" + model.id + "'" : "";
                        alerts.show({ type: "error", text: "Failed to " + action + " instance " + instanceId + ". " + JSON.parse(response.responseText).Error });
                    }
                });
                return false;
            },
            'change': function(event) {
                var target = event.target;
                var change = {};
                
                if (event.target.type === "checkbox") {
                    change[target.name] = $(target).is(':checked');
                } else {
                    change[target.name] = target.value.replace(/^\s+|\s+$/gm, '');
                }

                this.model.set(change);
            },
            'dispose': function() {
            }
        });

        return View;
    });