export interface Placement {

    pieceId:number;

    x:number;

    y:number;

    width:number;

    height:number;

    rotated:boolean;
}

export interface OptimizeResponse{

    usedLength:number;

    wasteRate:number;

    placements:Placement[];

}