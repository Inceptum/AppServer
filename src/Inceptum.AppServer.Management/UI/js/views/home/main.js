define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/home/main.html'
], function ($, _, Backbone, mainHomeTemplate) {

    var mainHomeView = Backbone.View.extend({
        tagName:"div",
        render:function () {
            $(this.el).html(mainHomeTemplate);
            return this;
        }
    });
    return new mainHomeView;
});
