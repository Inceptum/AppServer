define([
    'Underscore',
    'Backbone',
    'collections/bundles'
], function (_, Backbone, bundlesCollection) {
    var configurationsModel = Backbone.Model.extend({
        fetch: function(options) {

            var bundles=this.bundles;
            console.log("Fetchinging configuration");
            Backbone.Model.prototype.fetch.apply(this,[{
                success:function(){
                    console.log("Fetchinging configuration complete");
                    bundles.fetch(options);
                }
            }]);
        },

        parse: function(r){
            console.log("Parsing configuration");

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
                    process(r.bundlesmap,r.name);
            console.log("Parsing configuration complete");
                    return r;
                },
        url:function () {
            return "/configurations/" + this.get("id");
        },
        initialize:function (options) {
            this.bundles = new bundlesCollection([],{url:"/configurations/" + options.id+"/bundles"});
        }
    });


    return configurationsModel;

});
