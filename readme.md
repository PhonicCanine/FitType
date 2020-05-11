# FitType
A basic library for converting flat dictionary-like objects into heirarchical object objects.

For example, imagine a csv file with the following structure:

| Race Number | lane0Place | lane0Time | lane0laptime0 | lane0laptime1 | lane1Place | lane1Time | lane1laptime0 | lane1laptime1 |
|-------------|------------|-----------|---------------|---------------|------------|-----------|---------------|---------------|
| 0           | 1          | 2:00      | 1:00          | 1:00          | 2          | 3:30      | 1:45          | 1:45          |
| 1           | 2          | 3:00      | 1:30          | 1:30          | 1          | 2:00      | 1:00          | 1:00          |
| ...         | ...        | ...       | ...           | ...           | ...        | ...       | ...           | ...           |

Well, say we want to load this into two normal objects:

```cs
public class Race
{
    public int RaceID { get; set; }
    public List<Lane> lanes { get; set; }
}

public class Lane
{
    public int Place { get; set; }
    public TimeSpan Time { get; set; }
    public List<TimeSpan> LapTimes { get; set; }
}
```

With only basic annotation, and by calling `FitType.CoerceFitType<Race>(data)`, this can easily be read into normal objects:

```cs
public class Race
{
    [Prefix("Race Number")]
    public int RaceID { get; set; }
    [Prefix("lane*")]
    public List<Lane> lanes { get; set; }
}

public class Lane
{
    public int Place { get; set; }
    public TimeSpan Time { get; set; }
    [Prefix("laptime*")]
    public List<TimeSpan> LapTimes { get; set; }
}
```

This library is case insensitive, and will navigate the prefixes like paths (ie, assuming that `lane0` is in a list (and has been marked as `lane*`), when trying to find the correct property or field for `lane0place`, the library will look specifically for a `place` property/field).

Type conversions are handled automatically (any types that have a `Parse` method built in will have that called when reading in data).

`TryFitType` will return a boolean indicating whether the type fitting has been a success.

A coded example is available in the Example folder.