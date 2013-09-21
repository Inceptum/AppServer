define(['jquery', 'backbone', 'underscore','models/bundle'], function($, Backbone, _,bundleModel){
    var Collection = Backbone.Collection.extend({
        model: bundleModel,
        comparator : function(bundle) {
            return bundle.get("Name");
        }
    });
    return Collection;
});