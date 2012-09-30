define([
    'jquery',
    'backbone',
    'underscore',
    'models/applicationInstance',
    'text!templates/instanceEdit.html'],
    function($, Backbone, _, ApplicationInstanceModel, template){
        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                _(this).bindAll('submit');
                this.application=this.options.application;
                if(!this.model)
                    this.model=new ApplicationInstanceModel({"ApplicationId":this.application.id});
            },
            events:{
                "change":"change",
                "click #submit":"submit"
            },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                var versionSelect = $(this.el).find("#inputVersion");
                this.application.versions.each(function(version){$("<option></option>").text(version.id).attr("value",version.id).appendTo(versionSelect);})
            },
            'submit':function(e){
                e.preventDefault();
                var self = this;
                this.model.save(null, {
                    success: function (model) {
                        self.render();
/*
                        app.navigate('wines/' + model.id, false);
                        utils.showAlert('Success!', 'Wine saved successfully', 'alert-success');
*/
                    },
                    error: function () {
/*
                        utils.showAlert('Error', 'An error occurred while trying to delete this item', 'alert-error');
*/
                    }
                });
                return false;
            },
            'change':function(){
                // Apply the change to the model
                var target = event.target;
                var change = {};
                change[target.name] = target.value;
                this.model.set(change);

            /*    // Run validation rule (if any) on changed item
                var check = this.model.validateItem(target.id);
                if (check.isValid === false) {
                    utils.addValidationError(target.id, check.message);
                } else {
                    utils.removeValidationError(target.id);
                }*/
            },
            'dispose':function(){
            }
        });

        return View;
    });
