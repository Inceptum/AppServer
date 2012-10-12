define(['jquery', 'backbone', 'underscore','collections/bundles'], function($, Backbone, _){
    var Model = Backbone.Model.extend({
        initialize: function(){
            var BundlesCollection=require("collections/bundles");
            var bundles = this.get("bundles");
            if (bundles){
                this.bundles = new BundlesCollection(bundles);
                this.unset("bundles");
            }
            var model=this;
            this.bind('change', function(){
                if(model.hasChanged("bundles")){
                    model.bundles.update(model.get("bundles"));
                    this.unset("bundles");
                }
            });


        },
        url:function () {
            return "/api/configurations/" + this.get("configuration") + "/" + (this.isNew()?"":this.id);
        }
    });
    return Model;
});