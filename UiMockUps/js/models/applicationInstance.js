define(['jquery', 'backbone', 'underscore','context'], function($, Backbone, _,context){

    var Model = Backbone.Model.extend({
        urlRoot: context.httpUrl("/api/instance"),
        initialize: function() {
        },/*
        url:function () {
            return "/instances/" + this.get("Name");
        },*/
        idAttribute: "Name"

    });

    return Model;
});
