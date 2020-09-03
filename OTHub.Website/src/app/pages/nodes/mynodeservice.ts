import { MyNodeModel } from './mynodemodel';
import { Injectable } from '@angular/core';
import { HubHttpService } from '../hub-http-service';

@Injectable({
    providedIn: 'root',
})
export class MyNodeService {

    private items: { [key: string]: MyNodeModel };



    constructor(private hub: HubHttpService) {
        this.items = {};
        this.Load();
    }

    Add(myNode: MyNodeModel): boolean {
        myNode.Identity = myNode.Identity.trim();
        this.Load();
        if (!this.has(myNode.Identity)) {
            this.items[myNode.Identity] = myNode;
            this.Save();
            return true;
        }
        return false;
    }

    Load() {
        const myNodesText = localStorage.getItem('OTHub_MyNodes');
        if (myNodesText) {
            const parsedObjs = JSON.parse(myNodesText);
            // tslint:disable-next-line:prefer-for-of
            for (let i = 0; i < Object.keys(parsedObjs).length; i++) {
                const parsedObj = Object.values(parsedObjs)[i] as Object;
                const model = new MyNodeModel();
                // tslint:disable-next-line:forin
                for (const propertyName in parsedObj) {
                    model[propertyName] = parsedObj[propertyName];
                 }
                // model.Identity = parsedObj.Identity;
                // model.DisplayName = parsedObj.DisplayName;
                this.items[model.Identity] = model;
              }
        }
    }

    Save() {
        const data = JSON.stringify(this.items);
        localStorage.setItem('OTHub_MyNodes', data);
    }

    Remove(myNode: MyNodeModel) {
        this.Load();
        myNode.Identity = myNode.Identity.trim();
        if (this.has(myNode.Identity)) {
            delete this.items[myNode.Identity];
            this.Save();
        }
    }

    Get(identity: string): MyNodeModel {
        if (!identity) {
            return null;
        }

        if (this.hub.IsTestNet === true && identity === '0x464ff4f92806d75489624eb7658d06f88725cb48') {
            const model = new MyNodeModel();
            model.Identity = '0x464ff4f92806d75489624eb7658d06f88725cb48';
            model.DisplayName = 'OT Hub (Testnet Node)';
            return model;
        }

        return this.items[identity];
    }

    GetName(identity: string, returnAsEmpty: boolean): string {

        identity = identity?.trim();

        if (identity) {
            const item = this.items[identity];
            if (item) {
                if (item.DisplayName) {
                    return item.DisplayName;
                }
            } else if (this.hub.IsTestNet === true && identity === '0x464ff4f92806d75489624eb7658d06f88725cb48') {
                return 'OT Hub (Testnet Node)';
            }
        }

        if (returnAsEmpty) {
            return null;
        }

        return identity;
    }

    GetAll(): { [key: string]: MyNodeModel } {
        return this.items;
    }

    has(identity: string): boolean {
        return identity in this.items;
    }
}
