import { HttpClient, HttpHeaders } from "@angular/common/http";

export abstract class BaseService {
    constructor(protected http: HttpClient) {
        
    }

    protected headers(): HttpHeaders {
        let headers = new HttpHeaders();
        headers = headers.append("Content-Type", "application/json");
        headers = headers.append("Accept", "application/json");
        headers = headers.append("Access-Control-Allow-Origin", "*");
    
        const jwt = localStorage.getItem("token");
    
        if (jwt) {
          headers = headers.append("Authorization", "Bearer " + jwt);
        }
    
        return headers;
      }
}