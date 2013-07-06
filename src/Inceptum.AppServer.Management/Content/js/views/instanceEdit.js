define([
    'jquery',
    'backbone',
    'underscore',
    'models/instance',
    'text!templates/instanceEdit.html',
    'views/alerts'],
    function($, Backbone, _, instanceModel, template,alerts){
        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
                _(this).bindAll('submit');
                this.application=this.options.application;
                if(!this.model)
                    this.model=new instanceModel({"ApplicationId":this.application.attributes.Name,"ApplicationVendor":this.application.attributes.Vendor});
                else
                    this.model=this.model.clone();
            },
            events:{
                "change":"change",
                "click #submit":"submit"
            },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);

                var action = this.model.isNew()?"Create":"Save";
                this.$("#submit strong").text(action);
                var versionSelect = $(this.el).find("#inputVersion");
                var self=this;
                this.application.versions.each(function(version){
                    var option = $("<option></option>");
                    if(self.model.get("Version")===version.id)
                        option.attr("selected","selected")
                    option.text(version.id).attr("value",version.id).appendTo(versionSelect);
                });
            },
            'submit':function(e){
                e.preventDefault();
                var self = this;
                var action=this.model.isNew()?"create":"update";

                this.model.save(null, {
                    success: function (model) {
                        alerts.show({type:"info",text:"Instance '"+model.get("Name")+"' "+action+"d"});
                        self.navigate('#/applications/'+model.get("ApplicationVendor")+'/'+model.get("ApplicationId"), true);
                    },
                    error: function (model,response) {
                        var instanceId=action==="update"?"'"+model.id+"'":"";
                        alerts.show({type:"error",text:"Failed to "+action+" instance "+instanceId+". "+JSON.parse(response.responseText).Error});
                    }
                });
                return false;
            },
            'change':function(event){
                // Apply the change to the model
                var target = event.target;
                var change = {};
                if(event.target.type=="checkbox")
                    change[target.name] = target.value!="";
                else
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
