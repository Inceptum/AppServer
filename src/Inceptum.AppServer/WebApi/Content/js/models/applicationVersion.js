define(['jquery', 'backbone', 'underscore'], function($, Backbone, _){
    var Model = Backbone.Model.extend({
        idAttribute: "version"
    });

    return Model;
});
