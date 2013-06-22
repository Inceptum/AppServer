define(['jquery', 'backbone', 'underscore','models/bundle'], function($, Backbone, _,bundleModel){
    var Collection = Backbone.Collection.extend({
        model: bundleModel
    });
    return Collection;
});