define(['jquery', 'backbone', 'underscore','models/applicationVersion'], function($, Backbone, _, ApplicationVersionModel){
    var Collection = Backbone.Collection.extend({
        model:ApplicationVersionModel
    });


    return Collection;
});
