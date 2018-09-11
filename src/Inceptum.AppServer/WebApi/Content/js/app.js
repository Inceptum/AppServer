// This is the main entry point for the App
define(['routers/home','jquery','services/notificationsListener'], function(Router,jQuery,notificationsListener){
    Backbone.View.prototype.dispose = function(){
    }

    Backbone.View.prototype.close = function(){
        this.unbind();
        this.undelegateEvents();
        $(this.el).html('');
        this.dispose();

    }

    // When you have an existing set of models in a collection,
    // you can do in-place updates of these models, reusing existing instances.
    // - Items are matched against existing items in the collection by id
    // - New items are added
    // - matching models are updated using set(), triggers 'change'.
    // - existing models not present in the update are removed if 'removeMissing' is passed.
    // - a collection change event will be dispatched for each add() and remove()
    Backbone.Collection.prototype.update = function(models, options) {
        models  || (models = []);
        options || (options = {});

        //keep track of the models we've updated, cause we're gunna delete the rest if 'removeMissing' is set.
        var updateMap = _.reduce(this.models, function(map, model){ map[model.id] = false; return map },{});

        _.each( models, function(model) {

            var idAttribute = this.model.prototype.idAttribute;
            if(!(idAttribute instanceof Array))
                idAttribute=[idAttribute];
            //handling complex ids
            var modelId = _.map(idAttribute, function(attr){ return model[attr]; }).join('-');

            if ( modelId == undefined ) {
                throw new Error("Can't update a model with no id attribute. Please use 'reset'.");
            }

            if ( this._byId[modelId] ) {
                var attrs = (model instanceof Backbone.Model) ? _.clone(model.attributes) : _.clone(model);
                delete attrs[idAttribute];
                this._byId[modelId].set( attrs );
                updateMap[modelId] = true;
            }
            else {
                this.add( model );
            }
        }, this);

        if ( options.removeMissing ) {
            _.select(updateMap, function(updated, modelId){
                if (!updated) this.remove( modelId );
            }, this);
        }

        return this;
    },

    // Fetch the default set of models for this collection, resetting the
    // collection when they arrive. If `add: true` is passed, appends the
    // models to the collection instead of resetting.
    Backbone.Collection.prototype.fetch = function(options) {
        options || (options = {});
        var collection = this;
        var success = options.success;
        options.success = function(resp, status, xhr) {
            collection[options.update ? 'update' : options.add ? 'add' : 'reset'](collection.parse(resp, xhr), options);
            if (success) success(collection, resp);
        };
        options.error = Backbone.wrapError.call(this,options.error, collection, options);
        return (this.sync || Backbone.sync).call(this, 'read', this, options);
    }


    var init = function(){
        notificationsListener.init();
        var router = new Router();
        this.router = router;
        Backbone.View.prototype.navigate = function (loc) {
            router.navigate(loc, true);
        };
	};
	
	return { init: init};
});
