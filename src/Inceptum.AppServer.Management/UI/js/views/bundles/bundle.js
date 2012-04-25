define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/bundles/bundle.html'
], function ($, _, Backbone, bundleTemplate) {
    var bundleView = Backbone.View.extend({
        tagName:"div",
        initialize:function () {
            _.bindAll(this, "render");
            this.model.bind('change', this.render, this);
            this.router = this.options.router;
            this.configuration = this.options.configuration;
        },
        events:{
            "click .save":"saveBundle",
            "click .delete":"deleteBundle",
            "click .new":"newBundle"
        },

        newBundle:function () {
            var subBundleName = $("#subBundleName").val();
            if(!subBundleName || subBundleName.trim()===""){
                alert("wrong name: '"+subBundleName+"'");
                return;
            }

            this.router.createBundle(this.model.get('id')+'.'+subBundleName);
        },
        saveBundle:function () {
            this.model.set({
                Content:$('#bundleContent').val()
            });

            var self = this;
            if (this.model.isNew()) {
                this.configuration.bundles.create(this.model);
            } else {
                this.model.save({}, {
                    success:function () {
                        self.configuration.fetch({success:function () {
                            self.router.showBundle(self.configuration, self.model.get("id"));
                        }});
                        self.router.showMessage("success", "Bundle '" + self.model.get("id") + "' saved");

                    },
                    error:function (originalModel, resp) {
                        self.router.showMessage("warning", "Failed to save bundle '" + self.model.get("id") + "': " + resp.responseText);

                    }
                });
            }
            return false;
        },
        deleteBundle:function () {
            var self = this;
            this.model.destroy({
                success:function () {
                    self.router.showMessage("success", "Bundle '" + self.model.get("id") + "' deleted");
                    self.router.showConfiguration();
                },
                error:function (originalModel, resp) {
                    self.router.showMessage("warning", "Failed to delete bundle '" + self.model.get("id") + "': " + resp.responseText);
                }
            });
            return false;
        },
        render:function () {
            var data = {
                bundle:this.model,
                _:_
            };
            var compiledTemplate = _.template(bundleTemplate, data);
            $(this.el).html(compiledTemplate);
            return this;
        }
    });
    return bundleView;
});

