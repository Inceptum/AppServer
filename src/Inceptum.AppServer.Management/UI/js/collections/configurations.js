define([
    'jQuery',
    'Underscore',
    'Backbone'
    , 'models/configuration'
], function ($, _, Backbone, configurationsModel) {
    var configurationsCollection = Backbone.Collection.extend({
        model:configurationsModel,
        url:'http://localhost:9223/configurations',
        initialize:function () {
        }/*,
        parse: function(r){
            function process(nodes, conf){
                _(nodes).each(function(n){
                    _(n).extend({
                        metadata:{href:"#/configurations/"+conf+"/"+n.id},
                        data : { title : n.name, attr : {href:"#/configurations/"+conf+"/"+n.id}},
                        state:"open"
                    })
                    process(n.children,conf);
                });
            }
            _(r).each(function(cfg){process(cfg.bundlesmap,cfg.name)})
            return r;
        }*/

    });

    return new configurationsCollection;
});
