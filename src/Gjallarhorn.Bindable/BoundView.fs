﻿namespace Gjallarhorn.Bindable

open Gjallarhorn
open Gjallarhorn.Internal
open System.ComponentModel
open System.Reflection

/// An IView<'a> bound to a property on a source. This uses INotifyPropertyChanged to update the view as needed
type BoundView<'a>(name, initialValue, source : INotifyPropertyChanged) =
    let value = Mutable.create initialValue        

    let getValue () =
        let pi = source.GetType().GetRuntimeProperty(name)
        match pi with
        | null -> None
        | prop ->
            let v = prop.GetValue(source)
            downcastAndCreateOption(v)

    let updateValue v =
        match v with 
        | None -> ()
        | Some v' ->
            value.Value <- v'

    let handler (pch : PropertyChangedEventArgs) =
        match pch.PropertyName with
        | n when n = name ->
            getValue() 
            |> updateValue
        | _ -> ()

    let subscription = 
        source.PropertyChanged.Subscribe handler

    // TODO: Change to use dependencies without SM?
    interface System.IObservable<'a> with
        member __.Subscribe obs = value.Subscribe obs

    interface ITracksDependents with
        member __.Track dep = value.Track dep
        member __.Untrack dep = value.Untrack dep

    interface IDisposableView<'a> with
        member __.Value with get() = value.Value
        
    interface System.IDisposable with
        member this.Dispose() =
            subscription.Dispose()            
            
