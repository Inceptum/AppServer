define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){

    var Model = Backbone.Model.extend({
        urlRoot: context.httpUrl("/api/instance"),
        initialize: function() {
        },
        idAttribute: "Id"

    });

    return Model;
});
