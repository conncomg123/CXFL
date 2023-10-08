#include "../include/Frame.h"
#include <iostream>
enum KeyMode {
	KEY_MODE_NORMAL = 9728,
	KEY_MODE_CLASSIC_TWEEN = 22017,
	KEY_MODE_SHAPE_TWEEN = 17922,
	KEY_MODE_MOTION_TWEEN = 8195,
	KEY_MODE_SHAPE_LAYERS = 8192
};

void Frame::loadElements(pugi::xml_node& frameNode) {
	auto elements = frameNode.child("elements").children();
	for (auto iter = elements.begin(); iter != elements.end(); ++iter) {
		std::string type = iter->name();
		if (type.find("SymbolInstance") != std::string::npos) {
			this->elements.push_back(std::make_unique<SymbolInstance>(*iter));
		}
	}
}
Frame::Frame(pugi::xml_node& frameNode, bool isBlank) {
	this->root = frameNode;
	this->startFrame = frameNode.attribute("index").as_int();
	this->duration = frameNode.attribute("duration").empty() ? 1 : frameNode.attribute("duration").as_int();
	this->labelType = frameNode.attribute("labelType").value();
	this->name = frameNode.attribute("name").value();
	if (!isBlank) loadElements(frameNode);
}

// copy constructor, make a deep copy of the frame
Frame::Frame(const Frame& frame, bool isBlank) {
	// use the parent of this->root to insert the copy
	auto parent = frame.root.parent();
	if (!isBlank) {
		this->root = parent.insert_copy_after(frame.root, frame.root);
		this->root.remove_attribute("name");
		this->root.remove_attribute("labelType");
		this->root.remove_attribute("bookmark");
		loadElements(this->root);
	}
	else {
		this->root = parent.insert_child_after("DOMFrame", frame.root);
		this->root.append_attribute("index").set_value(frame.getStartFrame());
		this->root.append_attribute("duration").set_value(frame.getDuration());
		this->root.append_attribute("keyMode").set_value(KeyMode::KEY_MODE_NORMAL);
	}
	this->setStartFrame(frame.getStartFrame());
	this->setDuration(frame.getDuration());
	this->setLabelType(frame.getLabelType());
	this->setName(frame.getName());
}
Frame::~Frame() {

}
Element* Frame::getElement(unsigned int index) const {
	return elements[index].get();
}
unsigned int Frame::getDuration() const {
	return this->duration;
}
void Frame::setDuration(unsigned int duration) {
	// if duration is 1, we need to remove the attribute if it exists, else we need to set it
	if (duration == 1) this->root.remove_attribute("duration");
	else this->root.attribute("duration").set_value(duration);
	this->duration = duration;
}
unsigned int Frame::getStartFrame() const {
	return this->startFrame;
}
void Frame::setStartFrame(unsigned int startFrame) {
	this->root.attribute("index").set_value(startFrame);
	this->startFrame = startFrame;
}
std::string Frame::getLabelType() const {
	return this->labelType;
}
void Frame::setLabelType(const std::string& labelType) {
	this->root.attribute("labelType").set_value(labelType.c_str());
	this->labelType = labelType;
}
std::string Frame::getName() const {
	return this->name;
}
void Frame::setName(const std::string& name) {
	this->root.attribute("name").set_value(name.c_str());
	this->name = name;
}
bool Frame::isEmpty() const {
	return this->elements.empty();
}
pugi::xml_node& Frame::getRoot() {
	return this->root;
}