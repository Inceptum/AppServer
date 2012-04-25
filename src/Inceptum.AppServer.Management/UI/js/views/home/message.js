define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/home/message.html'
], function ($, _, Backbone, messageTemplate) {

    var messageView = Backbone.View.extend({
        render:function () {

            var data = {
                message:this.model,
                _:_
            };
            var compiledTemplate = _.template(messageTemplate, data);
            $(this.el).html(compiledTemplate);
            return this;
        }
    });
    return messageView;
});
