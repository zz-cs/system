var lib = require('lib');
var layout = require('webapp/core/layout.js');
var router = require('webapp/core/router.js');

var homeApplication = lib.marionette.Application.extend({

    initialize: function(options) {
        this.layout = new layout();
        this.layout.on('navigate', this._loadPage, this);

        this.router = new router();
        this.router.on('navigate', this._loadPage, this);
    },

    onStart: function() {
        this.layout.render();
        this.router.start();
    },

    onBeforeDestroy: function() {
        this.layout.destroy();
    },

    // api
    setContentView: function(view) {
        this.layout.setContentView(view);
    },

    navigate: function (route) {
        var args = Array.prototype.slice.call(arguments, 1);
        this._loadPage(route, args);
    },

    // private
    _loadPage: function(route, args) {
        var self = this;

        route = route || 'welcome';
        args = args || [];

        SystemJS.import(route).then(function(appSection) {
            self.appSection && self.appSection.destroy();

            var instance = self.appSection = new appSection({ application: self });
            instance.start.apply(instance, args);
            self.router.setPath(route, args);
        }).catch(function(err) {
            alert(err);
        });
    }
});

module.exports = homeApplication;
