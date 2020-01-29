import { User2Group } from "./user2group";
import { User2Role } from "./user2role";

export interface IUser {
    ObjId: string;
    InverseThis2UserGroups: User2Group[];
    InverseThis2Roles: User2Role[];
}
