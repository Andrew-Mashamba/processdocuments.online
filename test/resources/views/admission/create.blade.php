@extends('layouts.app')

@section('content')
<div class="container">
    <h1>Student Admission</h1>
    <form action="/admission" method="POST">
        @csrf
        <!-- Add form fields for student and guardian data -->
        <button type="submit">Submit</button>
    </form>
</div>
@endsection