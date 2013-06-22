define(['jquery', 'backbone', 'underscore'], function($, Backbone, _){
    var Model = Backbone.Model.extend({
        idAttribute: "Version"
    });

    return Model;
});
