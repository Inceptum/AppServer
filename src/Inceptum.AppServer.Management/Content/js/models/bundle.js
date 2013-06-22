define(['jquery', 'backbone', 'underscore','collections/bundles'], function($, Backbone, _){
    var Model = Backbone.Model.extend({
        initialize: function(){
            var BundlesCollection=require("collections/bundles");
            var bundles = this.get("Bundles");
            if (bundles){
                this.bundles = new BundlesCollection(bundles);
                this.unset("Bundles");
            }else{
                this.bundles = new BundlesCollection();
            }
            var model=this;
            this.bind('change', function(){
                if(model.hasChanged("Bundles")){
                    model.bundles.update(model.get("Bundles"));
                    model.unset("Bundles");
                }
            });


        },
        url:function () {
            return "/api/configurations/" + this.get("Configuration") + "/" + (this.isNew()?"":this.id);
        }
    });
    return Model;
});