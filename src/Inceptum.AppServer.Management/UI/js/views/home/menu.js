define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/home/menu.html'
], function($, _, Backbone, menuTemplate){

    var mainHomeView = Backbone.View.extend({
        tagName:"ul",
        render: function(){
            $(this.el).html(menuTemplate);
            $(this.el).find("li a").each(function(i,item){ $(item).addClass("active");});
            return this;
        }
    });
    return new mainHomeView;
});