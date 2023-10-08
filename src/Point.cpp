#include "../include/Point.h"
Point::Point(pugi::xml_node& pointNode) {
	this->root = pointNode;
	this->x = pointNode.attribute("x").as_double();
	this->y = pointNode.attribute("y").as_double();
}
Point::Point(const pugi::xml_node& pointNode) {
	this->root = pointNode;
	this->x = pointNode.attribute("x").as_double();
	this->y = pointNode.attribute("y").as_double();
}
Point::~Point() {

}
// responsibility of the caller to move this point's root somewhere else
Point::Point(const Point& point) {
	auto parent = point.root.parent();
	this->root = parent.insert_copy_after(point.root, point.root);
	this->setX(point.getX());
	this->setY(point.getY());
}
double Point::getX() const {
	return this->x;
}
void Point::setX(double x) {
	if (std::abs(x) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("x");
	else this->root.attribute("x").set_value(x);
}
double Point::getY() const {
	return this->y;
}
void Point::setY(double y) {
	if (std::abs(y) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("y");
	else this->root.attribute("y").set_value(y);
}
pugi::xml_node& Point::getRoot() {
	return this->root;
}