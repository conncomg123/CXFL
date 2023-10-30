#include "../include/Timeline.h"
void Timeline::loadLayers(pugi::xml_node& timelineNode) noexcept {
	auto layers = timelineNode.child("layers").children("DOMLayer");
	for (auto iter = layers.begin(); iter != layers.end(); ++iter) {
		this->layers.push_back(std::make_unique<Layer>(*iter));
	}
}
Timeline::Timeline(pugi::xml_node& timelineNode) noexcept {
	this->root = timelineNode;
	this->name = timelineNode.attribute("name").value();
	this->currentFrame = timelineNode.attribute("currentFrame").as_uint();
	loadLayers(timelineNode);
}
Timeline::~Timeline() {

}
void Timeline::setSelectedLayer(unsigned int index, bool appendToCurrentSelection) noexcept {
	if (!appendToCurrentSelection) {
		for (auto& layer : this->layers) {
			layer->setSelected(false);
		}
	}
	this->getLayer(index)->setSelected(true);
}
void Timeline::setCurrentLayer(unsigned int index) noexcept {
	for (auto& layer : this->layers) {
		layer->setCurrent(false);
	}
	this->getLayer(index)->setCurrent(true);
}
unsigned int Timeline::getFrameCount() const noexcept {
	unsigned int max = 0;
	for (auto& layer : this->layers) {
		if (layer->getFrameCount() > max) max = layer->getFrameCount();
	}
	return max;
}
unsigned int Timeline::getLayerCount() const noexcept {
	return this->layers.size();
}
unsigned int Timeline::addNewLayer(const std::string& name, const std::string& layerType) noexcept {
	// create the layer
	auto newChild = this->root.child("layers").append_child("DOMLayer");
	auto newLayer = std::make_unique<Layer>(newChild);
	newLayer->setName(name);
	newLayer->setLayerType(layerType);
	// if it's not a folder, insert one blank keyframe
	if (layerType != "folder") {
		auto newFrameChild = newChild.append_child("frames").append_child("DOMFrame");
		newFrameChild.append_attribute("index").set_value(0);
		newFrameChild.append_attribute("duration").set_value(this->getFrameCount());
		newFrameChild.append_child("elements");
		auto newFrame = std::make_unique<Frame>(newFrameChild, true);
		this->layers.push_back(std::move(newLayer));
		this->layers.back()->frames.push_back(std::move(newFrame));
	}
	// add the layer to the vector
	else this->layers.push_back(std::move(newLayer));
	return this->layers.size() - 1;
}
unsigned int Timeline::duplicateLayer(unsigned int index) noexcept {
	auto dupedLayer = std::make_unique<Layer>(*this->getLayer(index));
	dupedLayer->setName(dupedLayer->getName() + "_copy");
	this->layers.emplace(this->layers.begin() + index, std::move(dupedLayer));
	// for each layer after index, update the parentLayerIndex
	for (unsigned int i = index; i < this->layers.size(); i++) {
		if (this->layers[i]->getParentLayerIndex().has_value() && this->layers[i]->getParentLayerIndex() > index) {
			this->layers[i]->setParentLayerIndex(this->layers[i]->getParentLayerIndex().value() + 1);
		}
	}
	this->setSelectedLayer(index);
	this->setCurrentLayer(index);
	return index;
}
const std::vector<unsigned int> Timeline::findLayerIndex(const std::string& name) const noexcept {
	std::vector<unsigned int> indices;
	for (unsigned int i = 0; i < this->layers.size(); i++) {
		if (this->layers[i]->getName() == name) indices.push_back(i);
	}
	return indices;
}
void Timeline::deleteLayer(unsigned int index) noexcept {
	Layer* curLayer = this->layers[index].get();
	// if it's a folder, need a recursive delete since folders can contain other folders and layers
	if (curLayer->getLayerType() == "folder") {
		// delete all layers in the folder, we can determine if it's in the folder using the parentLayerIndex
		for (unsigned int i = index + 1; i < this->layers.size(); i++) {
			if (this->layers[i]->getParentLayerIndex().has_value() && this->layers[i]->getParentLayerIndex() == index) {
				this->deleteLayer(i);
				i--;
			}
		}
	}
	curLayer->getRoot().parent().remove_child(curLayer->getRoot());
	this->layers.erase(this->layers.begin() + index);
	// for each layer after index, update the parentLayerIndex
	for (unsigned int i = index; i < this->layers.size(); i++) {
		if (this->layers[i]->getParentLayerIndex().has_value() && this->layers[i]->getParentLayerIndex() > index) {
			this->layers[i]->setParentLayerIndex(this->layers[i]->getParentLayerIndex().value() - 1);
		}
	}
}
Layer* Timeline::getLayer(unsigned int index) const noexcept {
	return layers[index].get();
}
const std::string& Timeline::getName() const noexcept {
	return this->name;
}
void Timeline::setName(const std::string& name) noexcept {
	if (this->root.attribute("name").empty()) this->root.append_attribute("name");
	this->root.attribute("name").set_value(name.c_str());
	this->name = name;
}
unsigned int Timeline::getCurrentFrame() const noexcept {
	return this->currentFrame;
}
void Timeline::setCurrentFrame(unsigned int currentFrame) noexcept {
	if (this->root.attribute("currentFrame").empty()) this->root.append_attribute("currentFrame");
	this->root.attribute("currentFrame").set_value(currentFrame);
	this->currentFrame = currentFrame;
}
pugi::xml_node& Timeline::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Timeline::getRoot() const noexcept {
	return this->root;
}
