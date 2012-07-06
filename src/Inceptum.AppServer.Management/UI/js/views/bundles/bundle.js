define([
    'jQuery',
    'Underscore',
    'Backbone',
    'CodeMirror',
    'text!templates/bundles/bundle.html'
], function ($, _, Backbone, CodeMirror, bundleTemplate) {
    var bundleView = Backbone.View.extend({

        template:_.template(bundleTemplate),

        initialize:function () {
            this.configuration = this.options.configuration;
        },

        events:{
            "click .save":"saveBundle",
            "click .delete":"deleteBundle",
            "click .cancel":"cancel"
        },

        saveBundle:function () {
            this.model.set({
                Content:this.editor.getValue()
            });

            var self = this;
            if (this.model.isNew()) {
                this.configuration.bundles.create(this.model, {
                    success:function () {
                        self.configuration.fetch({success:function () {
                            Router.showBundle(self.configuration.id, self.model.id);
                        }});
                        Router.showMessage("success", "Bundle '" + self.model.id + "' created");
                    },
                    error:function (originalModel, resp) {
                        Router.showMessage("warning", "Failed to create bundle '" + self.model.id + "': " + resp.responseText);
                    }
                });
            } else {
                this.model.save({}, {
                    success:function () {
                        Router.showMessage("success", "Bundle '" + self.model.id + "' saved");
                        self.configuration.fetch({success:function () {
                            Router.showBundle(self.configuration.id, self.model.id);
                        }});
                    },
                    error:function (originalModel, resp) {
                        Router.showMessage("warning", "Failed to save bundle '" + self.model.id + "': " + resp.responseText);
                    }
                });
            }
            return false;
        },

        cancel:function () {
            Router.closeBundleView();
            Router.showConfiguration();
        },

        deleteBundle:function () {
            var self = this;
            if (window.confirm("Do you REALLY want to DELETE " + this.model.id + "?")) {
                this.model.destroy({
                    success:function () {
                        Router.showMessage("success", "Bundle '" + self.model.id + "' deleted");
                        Router.showConfiguration();
                    },
                    error:function (originalModel, resp) {
                        Router.showMessage("warning", "Failed to delete bundle '" + self.model.id + "': " + resp.responseText);
                    }
                });
            }
            return false;
        },

        render:function () {
            var data = {
                bundle:this.model,
                _:_
            };
            this.$el.html(this.template(data));
            this.setupEditor();
            return this;
        },

        setupEditor:function () {
            var editor = CodeMirror.fromTextArea(this.$el.find("#bundleContent").get(0), {
                mode:"javascript",
                json:true,
                smartIndent:false,
                fixedGutter:true,
                lineNumbers:true,
                matchBrackets:true,
                onCursorActivity:function () {
                    editor.setLineClass(hlLine, null);
                    hlLine = editor.setLineClass(editor.getCursor().line, "active-line");
                }
            });

            var hlLine = editor.setLineClass(0, "active-line");

            window.setTimeout(function () {
                editor.refresh();
            }, 10);

            this.editor = editor;
        }
    });

    return bundleView;
});

