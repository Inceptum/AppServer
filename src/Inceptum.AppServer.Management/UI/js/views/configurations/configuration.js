define([
    'jQuery',
    'Underscore',
    'Backbone',
    'text!templates/configurations/configuration.html',
    'order!libs/jquery/jsTree/jquery.jstree'
], function ($, _, Backbone, configurationTemplate) {
    var configurationView = Backbone.View.extend({
        tagName:"div",
        initialize:function () {
            _.bindAll(this, "render");

        },
        render:function () {
            var data = {
                configuration:this.model,
                _:_
            };
            var compiledTemplate = _.template(configurationTemplate, data);
            $(this.el).html(compiledTemplate);
            $(this.el).find(".bundles").jstree({
                core:{initially_open:true},
                plugins:["themes", "json_data", "ui"  ],
                json_data:{data:this.model.get("bundlesmap")}
            }).bind("select_node.jstree", function(e, data)
                                {
                                    if(jQuery.data(data.rslt.obj[0], "href"))
                                    {
                                        window.location.hash = jQuery.data(data.rslt.obj[0], "href");
                                    }
                                }).bind("before.jstree", function (e, data) {
                                		if(data.func === "open_node") {
                                			e.stopImmediatePropagation();
                                			return false;
                                		}
                                	});
            return this;
        }
    });
    return configurationView;
});

