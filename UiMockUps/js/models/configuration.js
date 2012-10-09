define(['jquery', 'backbone', 'underscore','collections/bundles'], function($, Backbone, _,BundlesCollection){
    var Model = Backbone.Model.extend({
        initialize: function(){
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
        }
    });
    return Model;
});