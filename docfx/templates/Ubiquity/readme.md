# Ubiquity DOCFX template
>[!NOTE]
> This markdown file is NOT included in the formal docs output. It is used to document
> the input to the process of creating the formal documentation.

This template adds support to the modern template to disable some features like inherited
members. It would be nice if docfx didn't even generate such things, but it has no knobs to
control that so it can only be disabled by a custom CSS to hide the content at page render
time on the client after it is generated and downloaded. :unamused:

## layout/_master.tmpl
This is mostly borrowed from the official DocFX `modern` template. However, the build
version number was added to the footer. Unfortunately no simpler means was found to do
that.

