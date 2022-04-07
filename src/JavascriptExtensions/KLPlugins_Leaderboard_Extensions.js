//// Get property `propname` for `pos`-th car overall.
//function Overall(overallPosition, propname) {
//    return $prop('LeaderboardPlugin.Overall.' + overallPosition + '.' + propname)
//}

//function InClass(classPosition, propname) {
//    var overallidx = $prop('LeaderboardPlugin.InClass.' + classPosition + '.OverallPosition')
//    if (overallidx == 0) return null;
//    if (propname == "OverallPosition") return overallidx;
//    return Overall(overallidx, propname)
//}

//// Get property `propname` for `pos`-th car in class.
////function InClass(classPosition, propname) {
////    var overallidx = $prop('LeaderboardPlugin.InClass.' + classPosition + '.OverallPosition')
////    if (overallidx == 0) return null;
////    if (propname == "OverallPosition") return overallidx;
////    return Overall(overallidx, propname)
////}

//// Get property `propname` for `pos`-th car relative to currently focused car in overall order. 
//// Note that `pos` starts from 1 and that would be the car that is `numRelPos` positions ahead 
//// of the focused car. `pos == numRelPos + 1` is the focused car and `pos == 2numRelPos + 1` is
//// the last car shown and `numRelPos` behind the focused car. The reason for this is that in 
//// SimHub you probably use repeated group to build the leaderboard and it's indexer `repeatindex()` 
//// starts at 1. So we also start counting at 1.
//function OverallRelativeToFocused(relativePosition, propname, numRelPos) {
//    var idx = relativePosition + $prop('LeaderboardPlugin.Focused.OverallPosition') - (numRelPos + 1)
//    if (propname == "OverallPosition") return idx;
//    return Overall(idx, propname)
//}

//// Get property `propname` for `pos`-th car relative to currently focused car in overall order 
//// or `pos`-th car overall if `pos < numOverallPos`. That is we show `numOverallPos` positions 
//// from the top of overall standings and then `numRelPos` realative positions around each side 
//// of focused car. See "Relative overall" screen on example dash.
//function OverallRelativeToFocusedPartial(relativePosition, propname, numRelPos, numOverallPos) {
//    var idx = relativePosition
//    var focusedIdx = $prop('LeaderboardPlugin.Focused.OverallPosition')
//    if (idx > numOverallPos && focusedIdx > numOverallPos + numRelPos) {
//        idx +=  focusedIdx - (numRelPos + 1) - numOverallPos
//    }
//    if (propname == "OverallPosition") return idx;
//    return Overall(idx, propname)
//}

//// Get property `propname` for `pos`-th car relative to currently focused car. Note that `pos` 
//// starts from 1. That is if `n` is the number of relative position specified in settings then
//// `pos=1` is the car that is `n` positions ahead of focused car. `pos == n+1` is the focused 
//// car and `pos == 2n+1` is the last car, `n` positions behind focused car.
//function Relative(relativePosition, propname) {
//    var overallidx = $prop('LeaderboardPlugin.Relative.' + relativePosition + '.OverallPosition')
//    if (overallidx == 0) return null;
//    if (propname == "OverallPosition") return overallidx;
//    return Overall(overallidx, propname)
//}

//// Get property `propname` for currently focused car.
//function Focused(propname) {
//    var overallidx =  $prop('LeaderboardPlugin.Focused.OverallPosition')
//    if (overallidx == 0) return null;
//    if (propname == "OverallPosition") return overallidx;
//    return Overall(overallidx, propname)
//}

//// Get property `propname` for the car that has best lap overall.
//function OverallBestLap(propname) {
//    var overallidx =  $prop('LeaderboardPlugin.Overall.BestLapCar.OverallPosition')
//    if (overallidx == 0) return null;
//    if (propname == "OverallPosition") return overallidx;
//    return Overall(overallidx, propname)
//}

//// Get property `propname` for the car that has best lap in class.
//function InClassBestLap(propname) {
//    var overallidx =  $prop('LeaderboardPlugin.InClass.BestLapCar.OverallPosition')
//    if (overallidx == 0) return null;
//    if (propname == "OverallPosition") return overallidx;
//    return Overall(overallidx, propname)
//}


//var ClassColors = {
//    GT3: "#00000000",
//    GT4: "#FF262660",
//    CUP17: "#FF457c45",
//    CUP21: "#FF284c28",
//    ST15: "#FFccba00",
//    ST21: "#FF988a00",
//    CHL: "#FFb90000",
//    TCX: "#FF007ca7",
//}

//var CupColors = {
//    Overall: "#FFFFFFFF",
//    ProAm: "#FF000000",
//    Am: "#FFe80000",
//    Silver: "#FF666666",
//    National: "#FF008f4b"
//}

//var CupTextColors = {
//    Overall: "#FF000000",
//    ProAm: "#FFFFFFFF",
//    Am: "#FF000000",
//    Silver: "#FFFFFFFF",
//    National: "#FFFFFFFF"
//}